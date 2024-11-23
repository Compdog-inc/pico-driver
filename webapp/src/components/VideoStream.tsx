import Skeleton from '@mui/joy/Skeleton';
import AspectRatio from '@mui/joy/AspectRatio';
import { useRef, useState, useEffect } from 'react';

export default function VideoStream() {
    const videoRef = useRef(null as HTMLVideoElement | null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (videoRef.current && videoRef.current.srcObject != null) {
            setLoading(false);
        }
    }, [videoRef.current]);

    return (
        <AspectRatio variant="soft" color="primary" sx={{ width: '100%', '--AspectRatio-radius': '13px' }} ratio={4 / 3}>
            <Skeleton loading={loading} animation="wave">
                <video ref={videoRef}></video>
            </Skeleton>
        </AspectRatio>
    );
}