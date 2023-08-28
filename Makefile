.PHONY: default
default: help

## help - Print help message.
.PHONY: help
help: Makefile
	@echo "usage: make <target>"
	@sed -n 's/^##//p' $<


.PHONY: test-data
testDataDir := eppo-sdk-test/files
test-data: 
	rm -rf $(testDataDir)
	mkdir -p $(testDataDir)
	gsutil cp gs://sdk-test-data/rac-experiments-*.json $(testDataDir)
	gsutil cp -r gs://sdk-test-data/assignment-v2 $(testDataDir)

.PHONY: build
build: 
	dotnet build --no-restore


.PHONY: test
test: 
	dotnet test --no-build --verbosity normal
