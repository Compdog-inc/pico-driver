import { StrictMode, PropsWithChildren } from 'react';
import { createRoot } from 'react-dom/client';
import { CssVarsProvider } from '@mui/joy/styles';
import CssBaseline from '@mui/joy/CssBaseline';
import { extendTheme } from '@mui/joy/styles';
import InitColorSchemeScript from '@mui/joy/InitColorSchemeScript';
import '@fontsource/inter/300.css';
import '@fontsource/inter/400.css';
import '@fontsource/inter/500.css';
import '@fontsource/inter/600.css';
import '@fontsource/inter/700.css';
import './_app.css';

export default function Main({ children }: PropsWithChildren) {
    const theme = extendTheme({
        colorSchemes: {
            light: {
                palette: {
                    primary: {
                        "50": '#F7EDFD',
                        "100": '#F1E3FB',
                        "200": '#E4C7F7',
                        "300": '#CC97F0',
                        "400": '#A26AC7',
                        "500": '#8654A7',
                        "600": '#643583',
                        "700": '#4D2A65',
                        "800": '#2D0A44',
                        "900": '#170523'
                    },
                    neutral: {
                        "50": '#FDFBFE',
                        "100": '#F5F0F8',
                        "200": '#E7DDEE',
                        "300": '#D9CDE1',
                        "400": '#A79FAD',
                        "500": '#6D6374',
                        "600": '#605568',
                        "700": '#39323E',
                        "800": '#1A171C',
                        "900": '#0D0B0E'
                    }
                }
            },
            dark: {
                palette: {
                    background: {
                        body: '#0C0A0E' // lighter background color
                    },
                    primary: {
                        "50": '#F7EDFD',
                        "100": '#F1E3FB',
                        "200": '#E4C7F7',
                        "300": '#CC97F0',
                        "400": '#A26AC7',
                        "500": '#8654A7',
                        "600": '#643583',
                        "700": '#4D2A65',
                        "800": '#2D0A44',
                        "900": '#170523'
                    },
                    neutral: {
                        "50": '#FDFBFE',
                        "100": '#F5F0F8',
                        "200": '#E7DDEE',
                        "300": '#D9CDE1',
                        "400": '#A79FAD',
                        "500": '#6D6374',
                        "600": '#605568',
                        "700": '#39323E',
                        "800": '#1A171C',
                        "900": '#0D0B0E'
                    }
                },
            }
        },
    });

    createRoot(document.getElementById('root')!).render(
        <StrictMode>
            <InitColorSchemeScript defaultMode="system" />
            <CssVarsProvider defaultMode="system" theme={theme}>
                <CssBaseline />
                {children}
            </CssVarsProvider>
        </StrictMode>,
    );
};
