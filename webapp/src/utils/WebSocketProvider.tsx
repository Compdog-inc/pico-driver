import { PropsWithChildren, createContext, useContext, useEffect, useState } from 'react';
import ReconnectingWebSocket from 'reconnecting-websocket';

export interface WebSocketContext {
    socket: ReconnectingWebSocket | null;
};

const _WebSocketContext = createContext({
    socket: null
} as WebSocketContext);

interface WebSocketProviderProps {
    protocols?: string | string[]
}

export const WebSocketProvider = ({ children, protocols }: PropsWithChildren<WebSocketProviderProps>) => {
    const [socket, setSocket] = useState(null as ReconnectingWebSocket | null);

    useEffect(() => {
        const ws = new ReconnectingWebSocket('ws://' + location.host, protocols);
        ws.binaryType = "arraybuffer";
        setSocket(ws);
        return () => ws.close();
    }, []);

    return (
        <_WebSocketContext.Provider value={{
            socket
        }} >
            {children}
        </_WebSocketContext.Provider>
    );
};

export const useWebSocket = () => {
    return useContext(_WebSocketContext);
};