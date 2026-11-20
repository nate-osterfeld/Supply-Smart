import { Navigate, Outlet } from 'react-router-dom';
import { useCurrentUser } from '../../hooks/useCurrentUser';

export default function RequireAuth() {
    if (!useCurrentUser()) {
        return <Navigate to="/" replace />;
    }

    // Otherwise, render the child route content
    return <Outlet />;
}
