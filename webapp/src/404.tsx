import Main from './_app';
//import styles from './404.module.css';
import Typography from '@mui/joy/Typography';
import Link from '@mui/joy/Link';

Main(
    {
        children:
            <>
                <Typography level="h1">404 - Page not found!</Typography>
                <Typography level="title-lg">
                    Go back <Link href="/">home</Link>
                </Typography>
            </>
    }
);