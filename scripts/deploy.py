#!/usr/bin/env python3

"""Deploy mod project, ignoring certain files/directories specified in a deployignore file."""

import sys
import pathlib
import shutil
import argparse
import textwrap
import logging

DEFAULT_DEPLOYIGNORE_PATH = pathlib.Path(".deployignore")

def find_workspace_root(path):
    """Use GitPython to try to find the workspace root."""
    try:
        import git
        repo = git.Repo(path, search_parent_directories=True)
        return pathlib.Path(repo.working_dir)

    except ImportError as exc:
        print("WARNING: Could not find workspace root automatically, since GitPython package is not installed. (pip install gitpython)")
    except git.InvalidGitRepositoryError as exc:
        print("WARNING: Not a valid git repository")

    return None

def parse_args(in_args):
    parser = argparse.ArgumentParser(description="Deploy project and remove anything unnecessary for a steam workshop upload.", 
                                     formatter_class=argparse.RawTextHelpFormatter)
    parser.add_argument('source', nargs='?', type=pathlib.Path, default=None, 
                        help=textwrap.dedent('''\
                             path to root of mod project. 
                             default: will use git to attempt to find it.
                             '''))
    parser.add_argument('dest', nargs='?', type=pathlib.Path, default=None, 
                        help=textwrap.dedent('''\
                             path to deploy to. 
                             default: '[source]/../[source]_deploy'
                             '''))
    parser.add_argument('-i', dest='deployignore', default=None,
                        help=textwrap.dedent('''\
                             path to ignore file listing all files/directories to be excluded from deployment.
                             default: '[source]/.deployignore'
                             '''))
    
    args = parser.parse_args(in_args)
    
    # supply defaults
    if args.source is None:
        args.source = find_workspace_root(pathlib.Path(__file__).parent)  # attempt to find workspace root from this python file's parent directory
        if args.source is None:
            print("---")
            parser.print_help()
            print("---\nCould not find mod directory automatically. Please specify a [source] via command arguments.")
            sys.exit(1)

    if args.dest is None:
        args.dest = args.source.parent / (args.source.name + "_deploy")

    if args.deployignore is None:
        args.deployignore = args.source / DEFAULT_DEPLOYIGNORE_PATH

    # final verify
    if not args.source.exists():
        raise FileExistsError(f"Could not find mod directory at '{args.source.resolve()}'!")
    if args.dest.exists():
        raise FileExistsError(f"'{args.dest}' already exists! Please remove before deploying!")
    if not args.deployignore.exists():
        raise FileExistsError(f"Could not find ignore file at '{args.source.resolve()}'!")

    # consolidate types and convert to absolute paths
    args.source = pathlib.Path(args.source).resolve()
    args.source = pathlib.Path(args.source).resolve()
    args.deployignore = pathlib.Path(args.deployignore).resolve()

    return args


def main(in_args=None):
    args = parse_args(in_args)
    
    print(f"Deploying to '{args.dest}'")
    
    ignores = []
    with open(args.deployignore, 'r') as in_stream:
        ignores = [ignore.strip() for ignore in in_stream.readlines()]

    # Copy this mod into deploy directory. I think this requires python 3.8?
    shutil.copytree(args.source, args.dest, ignore=shutil.ignore_patterns(*ignores))

    print("Deployed successfully!")
    
if __name__ == "__main__":
    main(sys.argv[1:])