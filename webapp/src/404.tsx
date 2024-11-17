import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './404.css'

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <>
            <h1>404 - Page not found!</h1>
            <div>
                Go back <a href="/">home</a>
            </div>
        </>
    </StrictMode>,
)
