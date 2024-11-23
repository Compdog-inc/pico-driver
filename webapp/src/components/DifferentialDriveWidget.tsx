import { useTheme } from '@mui/joy/styles';
import AspectRatio from '@mui/joy/AspectRatio';
import { PropsWithChildren } from 'react';

export default function DifferentialDriveWidget({ x, y }: PropsWithChildren<{ x: number, y: number }>) {
    const theme = useTheme();

    let paths;
    let gradients;

    if (y != 0) {
        x *= Math.sign(y);

        const x0 = 157;
        const y0 = y > 0 ? 66.5 : y < 0 ? 239.2 : 0;
        const x1 = x0 + 48 * x;
        const y1 = y0 - 52.5 * y;
        const cx = x0;
        const cy = y0 - 30 * y;
        const dx = x0 + 16.5 * x;
        const dy = y1;

        const g0 = y > 0 ? 8.5 : y < 0 ? 291.5 : 0;
        const g1 = g0 + Math.sign(y) * 49;

        paths = <>
            <path d={`M${x0} ${y0}C${cx} ${cy} ${dx} ${dy} ${x1} ${y1}`} stroke="url(#gradient)" strokeWidth="10" strokeLinecap="round" fill="none" />
        </>;

        gradients = <>
            <linearGradient id="gradient" x1="0" y1={g0} x2="0" y2={g1} gradientUnits="userSpaceOnUse">
                <stop stopColor={theme.vars.palette.primary[600]} stopOpacity="0.7" />
                <stop offset="0.355" stopColor={theme.vars.palette.primary[600]} stopOpacity="0.3465" />
                <stop offset="1" stopColor={theme.vars.palette.primary[600]} stopOpacity="0" />
            </linearGradient>
        </>;
    } else {
        const x0 = 24.6601 + 52;
        const y0 = 5.11105 + 109;
        const x1 = 19 + 52;
        const y1 = 76.0209 + 109;
        const cx = 2.71954 + 52;
        const cy = 25.571 + 109;
        const dx = -2.48294 + 52;
        const dy = 52.9833 + 109;

        const x2 = 219.8+5.105;
        const y2 = 76.132 + 109;
        const x3 = 219.8+5.80493;
        const y3 = 5 + 109;
        const ex = 219.8+25.5649;
        const ey = 54.1914 + 109;
        const fx = 219.8+28.8426;
        const fy = 26.4829 + 109;

        const g0 = 109;
        const g1 = g0 + 82;

        const dashOffset = 81 * (1 + x);

        paths = <>
            <path d={`M${x0} ${y0}C${cx} ${cy} ${dx} ${dy} ${x1} ${y1}`} stroke="url(#gradient_alt)" strokeWidth="10" strokeLinecap="round" strokeDasharray="81" strokeDashoffset={dashOffset} fill="none" />
            <path d={`M${x2} ${y2}C${ex} ${ey} ${fx} ${fy} ${x3} ${y3}`} stroke="url(#gradient)" strokeWidth="10" strokeLinecap="round" strokeDasharray="81" strokeDashoffset={dashOffset} fill="none" />
        </>;

        gradients = <>
            <linearGradient id="gradient" x1="0" y1={x < 0 ? g0 : g1} x2="0" y2={x < 0 ? g1 : g0} gradientUnits="userSpaceOnUse">
                <stop stopColor={theme.vars.palette.primary[600]} stopOpacity="0.7" />
                <stop offset="0.355" stopColor={theme.vars.palette.primary[600]} stopOpacity="0.3465" />
                <stop offset="1" stopColor={theme.vars.palette.primary[600]} stopOpacity="0" />
            </linearGradient>
            <linearGradient id="gradient_alt" x1="0" y1={x < 0 ? g1 : g0} x2="0" y2={x < 0 ? g0 : g1} gradientUnits="userSpaceOnUse">
                <stop stopColor={theme.vars.palette.primary[600]} stopOpacity="0.7" />
                <stop offset="0.355" stopColor={theme.vars.palette.primary[600]} stopOpacity="0.3465" />
                <stop offset="1" stopColor={theme.vars.palette.primary[600]} stopOpacity="0" />
            </linearGradient>
        </>;
    }

    return (
        <AspectRatio variant="plain" sx={{ width: '100%' }} ratio={1}>
            <svg viewBox="0 0 300 300">
                {paths}
                <rect x="102.5" y="75" width="95" height="150" rx="21" fill={`rgba(${theme.vars.palette.primary.mainChannel} / 0.4)`} />
                <defs>
                    {gradients}
                </defs>
            </svg>
        </AspectRatio>
    );
}