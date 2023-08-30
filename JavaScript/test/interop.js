﻿const assert = require("assert");
const dotnet = require("../dist/dotnet");
const { bootTest } = require("./csharp");

const invoke = (name, ...args) => dotnet.invoke("Test.Main", name, ...args);
const invokeAsync = (name, ...args) => dotnet.invokeAsync("Test.Main", name, ...args);

describe("interop when not booted", () => {
    it("throws when attempting to use", () => {
        const error = "Can't interop until .NET runtime is booted.";
        assert.throws(() => invoke("Foo"), error);
        assert.throws(() => invokeAsync("Foo"), error);
        assert.throws(() => dotnet.createObjectReference({}), error);
        assert.throws(() => dotnet.disposeObjectReference({}), error);
        assert.throws(() => dotnet.createStreamReference({}), error);
    });
});

describe("interop", () => {
    before(bootTest);
    after(dotnet.terminate);
    it("throws when assembly is not found", () => {
        assert.throws(() => dotnet.invoke("Foo", "JoinStrings"), /.*no loaded assembly.*'Foo'/);
    });
    it("throws when method is not found", () => {
        assert.throws(() => invoke("Bar"), /.*does not contain.*"Bar"/);
    });
    it("can send and receive string", () => {
        assert.deepStrictEqual(invoke("JoinStrings", "foo", "bar"), "foobar");
    });
    it("can send and receive number", () => {
        assert.deepStrictEqual(invoke("SumDoubles", -1, 2.75), 1.75);
    });
    it("can send and receive date", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(invoke("AddDays", date, 7));
        assert.deepStrictEqual(actual, expected);
    });
    it("can send and receive custom data type", () => {
        const expected = {
            wheeled: [
                { id: "car", wheelCount: 4, maxSpeed: 100.0 },
                { id: "bicycle", wheelCount: 2, maxSpeed: 30.5 }
            ],
            tracked: [
                { id: "tank", trackType: "Chain", maxSpeed: 20.005 },
                { id: "tractor", trackType: "Rubber", maxSpeed: 15.9 }
            ]
        };
        const actual = dotnet.invoke("Test.Types", "EchoRegistry", expected);
        assert.deepStrictEqual(actual, expected);
    });
    it("can invoke js function from dotnet", () => {
        let invoked = false;
        global.invokeFromDotNet = () => invoked = true;
        invoke("InvokeJS", "invokeFromDotNet");
        assert(invoked);
    });
    it("can invoke js function with array arg from dotnet", () => {
        let result;
        global.invokeFromDotNetWithArray = array => result = array;
        invoke("InvokeJSWithArray", "invokeFromDotNetWithArray", ["foo", "bar"]);
        assert.deepStrictEqual(result, ["foo", "bar"]);
    });
    it("can invoke async js function from dotnet", async () => {
        let invoked = false;
        global.asyncInvokeFromDotNet = () => new Promise(r => setTimeout(r, 1)).then(() => invoked = true);
        await invokeAsync("InvokeAsyncJS", "asyncInvokeFromDotNet");
        assert(invoked);
    });
    it("can process array with a js callback", () => {
        const array = ["a", "b", "c"];
        const expected = ["aa", "bb", "cc"];
        global.repeat = item => item + item;
        const actual = invoke("ForEachJS", array, "repeat");
        assert.deepStrictEqual(actual, expected);
    });
    it("can interop with async methods", async () => {
        assert.deepStrictEqual(await invokeAsync("JoinStringsAsync", "a", "b"), "ab");
    });
    it("can find method by alias", () => {
        assert.deepStrictEqual(invoke("EchoAlias", "foo"), "foo");
    });
    it("can interop with dotnet instance", () => {
        const instance = invoke("CreateInstance");
        assert.doesNotThrow(() => instance.invokeMethod("SetVar", "foo"));
        assert.deepStrictEqual(instance.invokeMethod("GetVar"), "foo");
        assert.doesNotThrow(() => instance.dispose());
    });
    it("can send dotnet instance back", () => {
        const instance1 = invoke("CreateInstance");
        const instance2 = invoke("CreateInstance");
        instance1.invokeMethod("SetVar", "bar");
        instance2.invokeMethod("SetFromOther", instance1);
        assert.deepStrictEqual(instance2.invokeMethod("GetVar"), "bar");
        instance1.dispose();
        instance2.dispose();
    });
    it("can return and receive js object", () => {
        const expected = dotnet.createObjectReference({ foo: "bar" });
        global.getObject = () => expected;
        const actual = invoke("GetAndReturnJSObject");
        assert.deepStrictEqual(actual, expected);
    });
    it("can interop with js object", async () => {
        const obj = {
            setField(value) { this.field = value; }
        };
        const ref = dotnet.createObjectReference(obj);
        await invokeAsync("InvokeOnJSObjectAsync", ref, "setField", ["nya"]);
        assert.deepStrictEqual(obj.field, "nya");
        assert.doesNotThrow(() => dotnet.disposeObjectReference(ref));
    });
    it("can send and receive raw bytes", () => {
        global.receiveBytes = bytes => new TextDecoder().decode(bytes);
        const bytes = new Uint8Array([0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
            0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
            0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
            0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e]);
        const expected = "Everything's shiny, Captain. Not to fret.";
        assert.deepStrictEqual(invoke("ReceiveBytes", bytes), expected);
        assert.deepStrictEqual(invoke("SendBytes"), expected);
    });
    it("can catch js exceptions", () => {
        global.throw = function () { throw new Error("foo"); };
        assert.deepStrictEqual(invoke("CatchException").split("\n")[0], "foo");
    });
    it("can catch dotnet exceptions", () => {
        assert.throws(() => invoke("Throw", "bar"), /Error: System.Exception: bar/);
    });
    it("can stream from js", async () => {
        const array = new Uint8Array(100000).map((_, index) => index % 256);
        const stream = dotnet.createStreamReference(array);
        await assert.doesNotReject(() => invokeAsync("StreamFromJSAsync", stream));
    });
    it("can't stream from dotnet", async () => {
        // https://github.com/Elringus/DotNetJS/issues/19
        const stream = invoke("StreamFromDotNet");
        await assert.rejects(stream.arrayBuffer(), /Streaming from .NET is not supported./);
    });
});
