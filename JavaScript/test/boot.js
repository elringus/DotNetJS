﻿const assert = require("assert");
const { boot, terminate, getBootStatus, BootStatus, invoke } = require("../dist/dotnet");
const { bootTest, getBootData } = require("./csharp");
const { Base64 } = require("js-base64");

describe("boot", () => {
    it("is in standby by default", () => {
        assert.deepStrictEqual(getBootStatus(), BootStatus.Standby);
    });
    it("throws when no boot data provided", async () => {
        await assert.rejects(boot, /Boot data is missing./);
    });
    it("throws when wasm binary is not provided", async () => {
        const data = { assemblies: [], entryAssemblyName: "" };
        await assert.rejects(boot(data), /Wasm binary is missing./);
    });
    it("throws when assembly data is not assigned", async () => {
        const assembly = { name: "Foo.dll" };
        const data = { wasm: new Uint8Array(1), assemblies: [assembly], entryAssemblyName: "Foo.dll" };
        await assert.rejects(boot(data), /Foo.dll assembly data is invalid./);
    });
    it("throws when assembly data length is zero", async () => {
        const assembly = { name: "Foo.dll", data: new Uint8Array(0) };
        const data = { wasm: new Uint8Array(1), assemblies: [assembly], entryAssemblyName: "Foo.dll" };
        await assert.rejects(boot(data), /Foo.dll assembly data is invalid./);
    });
    it("throws when attempting to boot while already booted", async () => {
        await bootTest();
        await assert.rejects(bootTest, /Invalid boot status. Expected: Standby. Actual: Booted./);
        terminate();
    });
    it("throws when attempting to boot while booting", async () => {
        const promise = bootTest();
        await assert.rejects(bootTest, /Invalid boot status. Expected: Standby. Actual: Booting./);
        await promise;
        terminate();
    });
    it("throws when attempting to terminate while not booted", () => {
        assert.throws(terminate, /Invalid boot status. Expected: Booted. Actual: Standby./);
    });
    it("boots when in standby", async () => {
        await bootTest();
        assert.deepStrictEqual(getBootStatus(), BootStatus.Booted);
        terminate();
    });
    it("invokes entry point on boot", async () => {
        await bootTest();
        assert(invoke("Test.Main", "IsMainInvoked"));
        terminate();
    });
    it("terminates when booted", async () => {
        await bootTest();
        terminate();
        assert.deepStrictEqual(getBootStatus(), BootStatus.Standby);
    });
    it("can boot with base64 binaries", async () => {
        const data = getBootData();
        data.wasm = Base64.fromUint8Array(data.wasm);
        for (let i = 0; i < data.assemblies.length; i++)
            data.assemblies[i].data = Base64.fromUint8Array(data.assemblies[i].data);
        await boot(data);
        assert.deepStrictEqual(getBootStatus(), BootStatus.Booted);
        terminate();
    });
});
