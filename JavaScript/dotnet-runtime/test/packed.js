﻿// noinspection JSCheckFunctionSignatures,JSUnresolvedFunction,JSUnresolvedVariable

const assert = require("assert");
const { packed, getGeneratedTypes, getGeneratedMap } = require("./csharp");

describe("packed library", () => {
    after(packed.terminate);
    it("throws on boot when a C#-declared function is missing implementation", async () => {
        await assert.rejects(packed.boot, /Function 'dotnet.Test.Main.EchoFunction' is not implemented\./);
    });
    it("allows providing implementation for functions declared in C#", () => {
        packed.Test.Main.EchoFunction = value => value;
    });
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(packed.boot);
        assert.deepStrictEqual(packed.getBootStatus(), packed.BootStatus.Booted);
    });
    it("re-exports dotnet members", async () => {
        assert(packed.BootStatus instanceof Object);
        assert(packed.getBootStatus instanceof Function);
        assert(packed.terminate instanceof Function);
        assert(packed.invoke instanceof Function);
        assert(packed.invokeAsync instanceof Function);
        assert(packed.createObjectReference instanceof Function);
        assert(packed.disposeObjectReference instanceof Function);
        assert(packed.createStreamReference instanceof Function);
    });
    it("provides exposed C# methods grouped under assembly object", async () => {
        assert.deepStrictEqual(packed.Test.Main.JoinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await packed.Test.Main.JoinStringsAsync("c", "d"), "cd");
    });
    it("can interop via functions declared in C#", async () => {
        assert.deepStrictEqual(packed.Test.Main.TestEchoFunction("a"), "a");
    });
    it("still can interop via strings", async () => {
        assert.deepStrictEqual(packed.invoke("Test.Project", "JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await packed.invokeAsync("Test.Project", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("generates valid type definitions", () => {
        const expectedLines = expectedTypes.split(/\r?\n/);
        const actualLines = getGeneratedTypes().split(/\r?\n/);
        for (const expectedLine of expectedLines)
            assert(actualLines.includes(expectedLine));
    });
    it("generates source map", () => {
        assert(getGeneratedMap());
    });
});

// TODO: That's fragile. Find a more robust way to validate the types.
const expectedTypes = `export interface Assembly {
    name: string;
    data: Uint8Array | string;
}
export interface BootData {
    wasm: Uint8Array | string;
    assemblies: Assembly[];
    entryAssemblyName: string;
}
export declare enum BootStatus {
    Standby = "Standby",
    Booting = "Booting",
    Terminating = "Terminating",
    Booted = "Booted"
}
export declare function getBootStatus(): BootStatus;
export declare function boot(): Promise<void>;
export declare function terminate(): Promise<void>;
export declare const invoke: <T>(assembly: string, method: string, ...args: any[]) => T;
export declare const invokeAsync: <T>(assembly: string, method: string, ...args: any[]) => Promise<T>;
export declare const createObjectReference: (object: any) => any;
export declare const disposeObjectReference: (objectReference: any) => void;
export declare const createStreamReference: (buffer: Uint8Array | any) => any;
export declare const Test: { Main: {
    EchoFunction: (value: string) => string,
    TestEchoFunction: (value: string) => string,
    CreateInstance: () => any,
    GetAndReturnJSObject: () => any,
    InvokeOnJSObjectAsync: (obj: any, fn: string, args: any) => Promise<void>,
    InvokeVoid: () => void,
    Echo: (message: string) => string,
    JoinStrings: (a: string, b: string) => string,
    SumDoubles: (a: number, b: number) => number,
    AddDays: (date: Date, days: number) => Date,
    InvokeJS: (funcName: string) => void,
    ForEachJS: (items: any, funcName: string) => any,
    JoinStringsAsync: (a: string, b: string) => Promise<string>,
    ReceiveBytes: (bytes: any) => string,
    SendBytes: () => string,
    GetGuid: () => string,
    CatchException: () => string,
    Throw: (message: string) => string,
    EchoViaWebSocket: (uri: string, message: string, timeout: number) => Promise<string>,
    ComputePrime: (n: number) => number,
    IsMainInvoked: () => boolean,
    StreamFromJSAsync: (streamRef: any) => Promise<void>,
    StreamFromDotNet: () => any,
    EchoRegistry: (registry: any) => any,
    CountTotalSpeed: (registry: any) => number,
};};
`;
