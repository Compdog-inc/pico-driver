import { encode, decode } from "@msgpack/msgpack";
import { useWebSocket } from './WebSocketProvider';
import { PropsWithChildren, useCallback, useEffect, useState, createContext, useContext } from 'react';

enum PacketType {
    ClientStatus,
    Joystick
};

export enum ControlMode {
    Manual,
    SemiAutonomous,
    Autonomous
};

export interface Joystick {
    x: number;
    y: number;
};

export interface DriverStation {
    connected: boolean;
    address: string | null;
    getServerTime: () => number
    sendTest: () => void,

    controlMode: ControlMode,
    setControlMode: (controlMode: ControlMode) => void,

    joystick: Joystick
};

interface DSClientStatus {
    address: string | null;
    connected: boolean;
    hasServerTime: boolean;
    serverTimeOffset: number;
};

const DriverStationContext = createContext({
    connected: false,
    address: null,
    getServerTime: () => 0,
    sendTest: () => { },

    controlMode: ControlMode.Manual,
    setControlMode: (_: ControlMode) => { },

    joystick: {
        x: 0,
        y: 0
    }
} as DriverStation);

export const DriverStationProvider = ({ children }: PropsWithChildren) => {
    const { socket } = useWebSocket();
    const [status, setStatus] = useState({
        address: null,
        connected: false,
        hasServerTime: false,
        serverTimeOffset: 0
    } as DSClientStatus);

    const [joystick, setJoystick] = useState({x: 0, y: 0} as Joystick);

    const [connected, setConnected] = useState(socket?.readyState == WebSocket.OPEN && status.connected);

    const [controlMode, setControlMode] = useState(ControlMode.Manual);

    useEffect(() => {
        const onMessage = (e: MessageEvent) => {
            const packet = decode(e.data) as any;
            switch (packet.type as PacketType) {
                case PacketType.ClientStatus: {
                    const status = packet as DSClientStatus;
                    setStatus(status);
                    setControlMode(ControlMode.Manual);
                    setConnected(socket?.readyState == WebSocket.OPEN && status.connected);
                    break;
                }
                case PacketType.Joystick: {
                    const joy = packet as Joystick;
                    if (Math.abs(joy.x) <= 2000)
                        joy.x = 0;
                    if (Math.abs(joy.y) <= 6000)
                        joy.y = 0;
                    setJoystick({
                        x: -joy.x / 32767,
                        y: joy.y / 32767
                    });
                    break;
                }
                default: {
                    console.warn("Unknown packet type!");
                    console.warn(packet);
                    break;
                }
            }
        };

        const onOpen = () => {
            setConnected(socket?.readyState == WebSocket.OPEN && status.connected);
        };

        const onClose = () => {
            setConnected(socket?.readyState == WebSocket.OPEN && status.connected);
        };

        if (socket) {
            socket.addEventListener('message', onMessage);
            socket.addEventListener('open', onOpen);
            socket.addEventListener('close', onClose);
            return () => {
                socket.removeEventListener('message', onMessage);
                socket.removeEventListener('open', onOpen);
                socket.removeEventListener('close', onClose);
            };
        }
    }, [socket]);

    const serverTimeCallback = useCallback(() => {
        return status.serverTimeOffset + ((performance.timeOrigin + performance.now()) * 1000);
    }, [status]);

    const sendTest = useCallback(() => {
        if (socket) {
            const buffer = encode({
                bob: "the builder",
                test: 123
            });

            socket.send(buffer);
        }
    }, [socket]);

    const setControlModeClient = useCallback((controlMode: ControlMode) => {
        setControlMode(controlMode);
    }, [setControlMode]);

    useEffect(() => {
        if (status.address)
            document.title = "Driver Station - " + status.address;
        else
            document.title = "Driver Station";
    }, [status.address]);

    return (
        <DriverStationContext.Provider value={{
            connected,
            address: status.address,
            getServerTime: serverTimeCallback,
            sendTest,
            controlMode,
            setControlMode: setControlModeClient,
            joystick
        }} >
            {children}
        </DriverStationContext.Provider>
    );
};

export const useDriverStation = () => {
    return useContext(DriverStationContext);
};