#!/bin/sh
echo -ne '\033c\033]0;DontWhackAMole\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/DontWhackAMole-v2.0.5.x86_64" "$@"
