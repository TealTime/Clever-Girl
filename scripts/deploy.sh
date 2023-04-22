#!/bin/bash
base_dir=$(git rev-parse --show-toplevel)
ignores=$(cat "${base_dir}/scripts/deployignore")

echo ${base_dir}
for ignore in ${ignores[@]}; do
    echo "${base_dir}/${ignores}"
done
