#!/bin/sh
echo -ne '\033c\033]0;DontWhackAMole\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/DontWhackAMole-v1.0.x86_64" "$@"
