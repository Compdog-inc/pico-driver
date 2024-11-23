import { ControlMode, useDriverStation } from '../utils/DriverStationProvider';
import Typography from '@mui/joy/Typography';
import Box from '@mui/joy/Box';
import Stack from '@mui/joy/Stack';
import Button from '@mui/joy/Button';
import ButtonGroup from '@mui/joy/ButtonGroup';
import VideoStream from './VideoStream';
import DifferentialDriveWidget from './DifferentialDriveWidget';
import { useRef, useEffect } from 'react';

export default function Sidebar() {
    const {
        connected, address, getServerTime,
        controlMode, setControlMode,
        joystick } = useDriverStation();

    const serverTimeRef = useRef(null as HTMLSpanElement | null);

    const serverTimeUs = connected ? getServerTime() : 0;
    const serverTimeMins = Math.floor(serverTimeUs / 60000000);
    const serverTimeSecs = Math.floor((serverTimeUs / 1000000) % 60);
    const serverTimeFrac = Math.floor((serverTimeUs / 1000) % 1000);

    useEffect(() => {
        const int = setInterval(() => {
            if (serverTimeRef.current) {
                const serverTimeUs = connected ? getServerTime() : 0;
                const serverTimeMins = Math.floor(serverTimeUs / 60000000);
                const serverTimeSecs = Math.floor((serverTimeUs / 1000000) % 60);
                const serverTimeFrac = Math.floor((serverTimeUs / 1000) % 1000);
                serverTimeRef.current.innerText = `Server time: ${serverTimeMins.toFixed(0).padStart(2, '0')}:${serverTimeSecs.toFixed(0).padStart(2, '0') }.${serverTimeFrac.toFixed(0).padStart(3, '0')}`;
            }
        }, 1);

        return () => {
            clearInterval(int);
        };
    }, [serverTimeRef.current, getServerTime, connected]);

    return (
        <Stack direction="column" alignItems="center" spacing={3}>
            <Typography level="body-lg" fontSize={36}>{address ?? "<Unknown>"}</Typography>
            <Typography level="body-lg">{connected ? "Connected" : "Not Connected"}</Typography>
            <Box display="flex" flexDirection="row" gap={2}>
                <Button variant="soft" sx={{
                    borderRadius: 50
                }} >Save</Button>
                <Button sx={{
                    borderRadius: 50
                }}>Restart</Button>
            </Box>
            <Stack direction="column" spacing={1} sx={{ width:'100%' }}>
                <Typography level="title-md" alignSelf="start">Live Video Stream</Typography>
                <VideoStream />
                <Typography level="body-sm" alignSelf="start" ref={serverTimeRef}>{`Server time: ${serverTimeMins.toFixed(0).padStart(2, '0')}:${serverTimeSecs.toFixed(0).padStart(2, '0')}.${serverTimeFrac.toFixed(0).padStart(3, '0')}`}</Typography>
            </Stack>
            <Typography level="title-md">Current control mode</Typography>
            <ButtonGroup
                color="primary"
                sx={{ '--ButtonGroup-radius': '40px' }}
            >
                <Button
                    variant={controlMode == ControlMode.Manual ? "solid" : undefined}
                    onClick={() => setControlMode(ControlMode.Manual)}
                >Manual</Button>
                <Button
                    variant={controlMode == ControlMode.SemiAutonomous ? "solid" : undefined}
                    onClick={() => setControlMode(ControlMode.SemiAutonomous)}
                >Semi-Autonomous</Button>
                <Button
                    variant={controlMode == ControlMode.Autonomous ? "solid" : undefined}
                    onClick={() => setControlMode(ControlMode.Autonomous)}
                >Autonomous</Button>
            </ButtonGroup>
            <DifferentialDriveWidget x={joystick.x} y={joystick.y} />
        </Stack>
    )
}