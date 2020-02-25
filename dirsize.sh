#!/usr/bin/env bash
cd "$1"

echo $(ls -1 | wc -l)

exit 0
