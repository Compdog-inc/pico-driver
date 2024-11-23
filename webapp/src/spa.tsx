//import styles from './spa.module.css';
import { DriverStationProvider } from './utils/DriverStationProvider';
import Box from '@mui/joy/Box';
import Sheet from '@mui/joy/Sheet';
import StatusWidget from './components/StatusWidget';
import Sidebar from './components/Sidebar';

function App() {
    return (
        <DriverStationProvider>
            <Box display="flex" flexDirection="row">
                <Box flex="1">
                    <StatusWidget />
                </Box>
                <Sheet color="neutral" variant="soft" sx={{
                    width: 420,
                    height: '100vh',
                    p: 2
                }}>
                    <Sidebar />
                </Sheet>
            </Box>
        </DriverStationProvider>
    )
}

export default App
