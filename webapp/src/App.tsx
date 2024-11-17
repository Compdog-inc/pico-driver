import './App.css'
import StatusWidget from './components/StatusWidget'
import { DriverStationProvider } from './utils/DriverStationProvider'


function App() {
    return (
        <DriverStationProvider>
            <StatusWidget />
        </DriverStationProvider>
    )
}

export default App
