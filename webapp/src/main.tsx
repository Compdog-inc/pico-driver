import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import { WebSocketProvider } from './utils/WebSocketProvider';
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <WebSocketProvider protocols={["driverstation.webapp.msgpack"]}>
            <App />
        </WebSocketProvider>
  </StrictMode>,
)
