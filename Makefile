.PHONY: default
default: help

## help - Print help message.
.PHONY: help
help: Makefile
	@echo "usage: make <target>"
	@sed -n 's/^##//p' $<




testDataDir := eppo-sdk-test/files
tempDir := ${testDataDir}/temp
gitDataDir := ${tempDir}/sdk-test-data
branchName := main
githubRepoLink := https://github.com/Eppo-exp/sdk-test-data.git

.PHONY: test-data
test-data: 
	rm -rf $(testDataDir)
	mkdir -p $(tempDir)
	git clone -b ${branchName} --depth 1 --single-branch ${githubRepoLink} ${gitDataDir}
	cp -r ${gitDataDir}/ufc ${testDataDir}/
	rm -rf ${tempDir}


.PHONY: build
build: 
	dotnet build


.PHONY: test
test: test-data
	dotnet test --verbosity normal
