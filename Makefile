.PHONY: up down down-v test run watch

up:
	docker compose up -d

down:
	docker compose down

down-v:
	docker compose down -v

test:
	dotnet test

run:
	dotnet run --project TheBugTracker.csproj

watch:
	dotnet watch run --project TheBugTracker.csproj --non-interactive
