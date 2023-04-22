import sys
import pathlib
import shutil
import argparse
import git

def parse_args(in_args):
    parser = argparse.ArgumentParser(description="Remove anything that shouldn't be uploaded via Steam Workshop")
    parser.add_argument('path', metavar='mod_directory', nargs='?', type=pathlib.Path, help='Path to the root of the mod project we want to deploy.', default=None)
    parser.add_argument('--ignore-file', '-i', dest='ignore_file', help='Path to an ignore file listing all files to be removed at deployment.', default=None)
    parser.add_argument('--disable-checks', dest='check', action='store_false', default=True, 
                        help='Disable following assertion: if (manifest.version is not beta && git_repo.branch is main), because I forget all the time to check this.')
    
    args = parser.parse_args(in_args)
    
    # supply defaults
    if args.path is None:
        try:
            repo = git.Repo(pathlib.Path(__file__).parent, search_parent_directories=True)
            args.path = pathlib.Path(repo.working_dir)
        except git.InvalidGitRepositoryError as exc:
            print("Could not find git directory from current working directory. Please manually supply one via command arguments.")
            parser.print_help()
            sys.exit(1)

    if args.ignore_file is None:
        args.ignore_file = args.path / "scripts" / "deployignore"

    # final verify
    if not args.path.exists():
        raise FileExistsError(f"Could not find mod directory at '{args.path.resolve()}'!")

    if not args.ignore_file.exists():
        raise FileExistsError(f"Could not find ignore file at '{args.path.resolve()}'!")

    # consolidate types and convert to absolute paths
    args.path = pathlib.Path(args.path).resolve()
    args.ignore_file = pathlib.Path(args.ignore_file).resolve()

    return args

def main(in_args=None):
    args = parse_args(in_args)
    
    ignore_paths = []
    with open(args.ignore_file, 'r') as in_stream:
        ignore_paths = [args.path / line.strip() for line in in_stream.readlines()]
        ignore_paths = [path for path in ignore_paths if path.exists()]

    print("Found these files that match ignore patterns:")
    print("\n".join(str(path) for path in ignore_paths))
    response = input("\nRemove these files/directories? (y/n) : ").lower()
    if response == 'y':
        for path in ignore_paths:
            if path.is_dir():
                shutil.rmtree(path)
            else:
                path.unlink()
    
if __name__ == "__main__":
    main(sys.argv[1:])