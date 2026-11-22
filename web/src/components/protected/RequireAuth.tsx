import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { billingReturnSearch } from '../../api/browserSession';

export default function RequireAuth() {
    const location = useLocation();
    if (!useCurrentUser()) {
        return <Navigate to={'/' + billingReturnSearch(location.search)} replace />;
    }

    // Otherwise, render the child route content
    return <Outlet />;
}
