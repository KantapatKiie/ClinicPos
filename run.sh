#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

case "${1:-}" in
	--detached)
		docker compose up --build -d
		;;
	--down)
		docker compose down
		;;
	"")
		docker compose up --build
		;;
	*)
		echo "Usage: ./run.sh [--detached|--down]"
		exit 1
		;;
esac
