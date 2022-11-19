﻿import { useEffect, useState } from "react";
import { Backend } from "backend";

type Props = {
    power: number;
};

export const Stress = (props: Props) => {
    const [stressing, setStressing] = useState(false);
    const [iterations, setIterations] = useState("");

    useEffect(() => {
        Backend.GetStressPower = () => props.power;
    }, [props.power]);

    useEffect(() => {
        Backend.OnStressComplete.subscribe(logIteration);
        return () => Backend.OnStressComplete.unsubscribe(logIteration);
    }, []);

    return (
        <div id="stress">
            <div>
                This sample shows the benefit of running dotnet on worker thread.
                The Donut is animating on the main (UI) thread and dotnet is running stress test.
                When built without 'CreateWorker' enabled, the animation will perform poorly.
            </div>
            <button onClick={toggleStress}>
                {stressing ? "STOP STRESS" : "START STRESS"}
            </button>
            <div id="iterations">
                {iterations}
            </div>
        </div>
    );

    function toggleStress() {
        if (Backend.IsStressing()) Backend.StopStress();
        else Backend.StartStress();
        setStressing(!stressing);
    }

    function logIteration(time: number) {
        setIterations(i => {
            if (i.length > 999) i = i.substring(0, i.lastIndexOf("\n"));
            const stamp = new Date().toLocaleTimeString([], { hour12: false });
            return `[${stamp}] Stressed over ${time}ms.\n${i}`;
        });
    }
};
