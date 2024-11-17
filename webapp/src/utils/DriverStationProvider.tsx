import { encode, decode } from "@msgpack/msgpack";
import { useWebSocket } from './WebSocketProvider';
import { PropsWithChildren, useCallback, useEffect, useState, createContext, useContext } from 'react';

interface DSClientStatus {
    address: string | null;
    connected: boolean;
    hasServerTime: boolean;
    serverTimeOffset: number;
};

export interface DriverStation {
    connected: boolean;
    address: string | null;
    getServerTime: () => number
    sendTest: () => void
};

const DriverStationContext = createContext({
    connected: false,
    address: null,
    getServerTime: () => 0,
    sendTest: () => { }
} as DriverStation);

export const DriverStationProvider = ({ children }: PropsWithChildren) => {
    const { socket } = useWebSocket();
    const [status, setStatus] = useState({
        address: null,
        connected: false,
        hasServerTime: false,
        serverTimeOffset: 0
    } as DSClientStatus);
    const [connected, setConnected] = useState(socket?.readyState == WebSocket.OPEN && status.connected);

    useEffect(() => {
        const onMessage = (e: MessageEvent) => {
            const packet = decode(e.data) as any;
            if (typeof (packet.address) !== 'undefined') {
                const status = packet as DSClientStatus;
                setStatus(status);
                setConnected(socket?.readyState == WebSocket.OPEN && status.connected);
            } else {
                console.log(packet);
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
        return status.serverTimeOffset;
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
            sendTest
        }} >
            {children}
        </DriverStationContext.Provider>
    );
};

export const useDriverStation = () => {
    return useContext(DriverStationContext);
};