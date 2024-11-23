import { useDriverStation } from '../utils/DriverStationProvider';
import Typography from '@mui/joy/Typography';
import Button from '@mui/joy/Button';

export default function StatusWidget() {
    const { connected, sendTest } = useDriverStation();

    return (
        <div>
            <Typography level="h1">Driver Station: {connected ? "Connected" : "Not Connected"}</Typography>
            <Button variant="solid" onClick={sendTest}>Send Message</Button>
        </div>
    )
}