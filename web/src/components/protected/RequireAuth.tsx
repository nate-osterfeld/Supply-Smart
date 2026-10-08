import { Navigate, Outlet } from 'react-router-dom';

export default function RequireAuth() {
    const token = localStorage.getItem('supplysmart_token');

    // If no token exists, redirect to login page
    if (!token) {
        return <Navigate to="/" replace />;
    }

    // Otherwise, render the child route content
    return <Outlet />;
}