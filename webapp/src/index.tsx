import Main from './_app';
import { WebSocketProvider } from './utils/WebSocketProvider.tsx';
import App from './spa.tsx';

Main(
    {
        children:
            <WebSocketProvider protocols={["driverstation.webapp.msgpack"]}>
                <App />
            </WebSocketProvider>
    }
);