import { useDriverStation } from '../utils/DriverStationProvider';

export default function App() {
    const { connected, sendTest } = useDriverStation();

    return (
        <div>
            <h1>Driver Station: {connected ? "Connected" : "Not Connected"}</h1>
            <button onClick={sendTest}>Send Message</button>
        </div>
    )
}