# Studio session storage

All three Studio modes share one two-megabyte UTF-8 byte budget. Persistence
keeps present artifacts and edit recovery first, then prunes historical states
until the exact serialized value passes the same schema and byte limit used by
restore. A rejected restore blocks automatic overwrite for that page session so
the interface cannot label lost state as saved.
