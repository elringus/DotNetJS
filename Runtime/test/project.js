﻿const dotnet = require("../dist/dotnet");
const path = require("path");
const fs = require("fs");
const assert = require("assert");

exports.bootTest = bootTest;

async function bootTest() {
    const bootData = {
        entryAssemblyName: "Test.dll",
        assemblies: loadAssemblies()
    };
    await dotnet.boot(bootData);
}

function loadAssemblies() {
    let assemblies = [];
    for (const assemblyPath of findAssemblies())
        assemblies.push(loadAssembly(assemblyPath));
    return assemblies;
}

function findAssemblies() {
    let assemblyPaths = [];
    const dirPath = resolveAssembliesDirectory();
    for (const fileName of fs.readdirSync(dirPath))
        if (fileName.endsWith(".dll"))
            assemblyPaths.push(`${dirPath}/${fileName}`);
    return assemblyPaths;
}

function resolveAssembliesDirectory() {
    const dirPath = path.resolve("test/project/bin/Release/net6.0/browser-wasm/publish");
    assert(fs.existsSync(dirPath), "Missing test assemblies. Run 'scripts/compile-test.sh'.");
    return dirPath;
}

function loadAssembly(assemblyPath) {
    return {
        name: path.parse(assemblyPath).base,
        data: fs.readFileSync(assemblyPath)
    };
}
