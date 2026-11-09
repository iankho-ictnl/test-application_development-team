#!/bin/sh
# Daily PostgreSQL backup (D-024, REQ-023: RPO 24 hours).
# Runs in the backup container. Writes a custom-format pg_dump every day at BACKUP_HOUR (UTC)
# to /backups/daily and, on the first day of the month, a copy to /backups/monthly.
# Keeps the newest 14 daily and 12 monthly dumps. Credentials come from the environment
# (PGHOST, PGUSER, PGPASSWORD, PGDATABASE); nothing secret is written here or logged.
set -eu

BACKUP_HOUR="${BACKUP_HOUR:-2}"
KEEP_DAILY="${KEEP_DAILY:-14}"
KEEP_MONTHLY="${KEEP_MONTHLY:-12}"
DAILY_DIR=/backups/daily
MONTHLY_DIR=/backups/monthly

mkdir -p "$DAILY_DIR" "$MONTHLY_DIR"

prune() {
	# Keep the newest $2 files in $1 (names sort by date).
	ls -1 "$1"/*.dump 2>/dev/null | sort -r | tail -n +"$(($2 + 1))" | while read -r old; do
		rm -f -- "$old"
	done
}

backup() {
	stamp="$(date -u +%Y-%m-%d)"
	target="$DAILY_DIR/${PGDATABASE}-${stamp}.dump"
	tmp="$target.partial"
	if pg_dump --format=custom --no-owner --file="$tmp"; then
		mv -f -- "$tmp" "$target"
		echo "$(date -u +%FT%TZ) backup written: $(basename "$target")"
		if [ "$(date -u +%d)" = "01" ]; then
			cp -f -- "$target" "$MONTHLY_DIR/$(basename "$target")"
		fi
		prune "$DAILY_DIR" "$KEEP_DAILY"
		prune "$MONTHLY_DIR" "$KEEP_MONTHLY"
	else
		rm -f -- "$tmp"
		echo "$(date -u +%FT%TZ) ERROR: backup failed" >&2
	fi
}

while true; do
	now="$(date -u +%s)"
	next="$(date -u -d "today ${BACKUP_HOUR}:00" +%s)"
	if [ "$next" -le "$now" ]; then
		next="$(date -u -d "tomorrow ${BACKUP_HOUR}:00" +%s)"
	fi
	sleep "$((next - now))"
	backup
done
