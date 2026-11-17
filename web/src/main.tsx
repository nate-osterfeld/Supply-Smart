import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import RequireAuth from './components/protected/RequireAuth';
import { initializeBrowserSession, readBrowserSession } from './api/browserSession';
import './index.css';

import AuthPage from './pages/auth/AuthPage';
import App from './App';

// Initialize TanStack Query Client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: 1,
    },
  },
});

// Configure React Router
initializeBrowserSession();
const router = createBrowserRouter([{
  id: 'browser-session',
  loader: readBrowserSession,
  hydrateFallbackElement: <main role="status">Checking your session…</main>,
  errorElement: <main role="alert">Unable to check your session. <button onClick={() => window.location.reload()}>Try again</button></main>,
  children: [{
    path: '/',
    element: <AuthPage />,
  },
  {
    // All routes nested under this element require authentication
    element: <RequireAuth />,
    children: [
      {
        path: '/dashboard',
        element: <App />,
      },
      // Future protected routes go here!
    ],
  }],
}]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>
);
