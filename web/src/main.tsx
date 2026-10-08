import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import RequireAuth from './components/protected/RequireAuth';
import './index.css';

import AuthPage from './pages/auth/AuthPage';

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
const router = createBrowserRouter([
  {
    path: '/',
    element: <AuthPage />,
  },
  {
    // All routes nested under this element require authentication
    element: <RequireAuth />,
    children: [
      {
        path: '/dashboard',
        element: (
          <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100">
            <div className="text-center">
              <h1 className="text-2xl font-bold">Welcome to SupplySmart Dashboard</h1>
              <p className="mt-2 text-sm text-slate-500">Workspace successfully unlocked!</p>
            </div>
          </div>
        ),
      },
      // Future protected routes (like inventory, catalog, settings) go here!
    ],
  },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>
);