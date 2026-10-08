import axios, {
    type AxiosError,
    type AxiosInstance,
    type AxiosRequestConfig,
    type AxiosResponse,
    type InternalAxiosRequestConfig,
} from 'axios';

export class BaseApi {
    protected api: AxiosInstance;
    protected baseUrl: string = '';
    protected usesAuth: boolean;

    public constructor(usesAuth: boolean = true) {
        this.usesAuth = usesAuth;
        this.baseUrl = import.meta.env.VITE_BASEURL || '';
        
        this.api = axios.create({
            headers: {
                'Content-Type': 'application/json',
                'Cache-Control': 'no-cache',
                Pragma: 'no-cache',
            }
        });

        // Request Interceptor: Automatically attach JWT token if required
        this.api.interceptors.request.use(
            async (request: InternalAxiosRequestConfig): Promise<InternalAxiosRequestConfig> => {
                if (this.usesAuth) {
                    const token = localStorage.getItem('supplysmart_token');
                    if (token && request.headers) {
                        request.headers.Authorization = `Bearer ${token}`;
                    }
                }
                return request;
            },
            (error: AxiosError) => Promise.reject(error)
        );

        // Response Interceptor: Handle global errors like 401 Unauthorized
        this.api.interceptors.response.use(
            (response: AxiosResponse) => response,
            (error: AxiosError) => {
                const errorMsg = this.getErrorMsg(error);
                if (error.response?.status === 401) {
                    localStorage.clear();
                    window.location.replace('/login');
                }
                return Promise.reject(new Error(errorMsg));
            }
        );
    }

    protected get<T>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
        return this.api.get<T>(`${this.baseUrl}/${url}`, config);
    }

    protected post<T>(url: string, data?: object | string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
        return this.api.post<T>(`${this.baseUrl}/${url}`, data, config);
    }

    private getErrorMsg(err: unknown): string {
        if (axios.isAxiosError(err)) {
            const axiosErr = err as AxiosError<any>;
            if (axiosErr.response?.data && typeof axiosErr.response.data === 'object' && 'message' in axiosErr.response.data) {
                return (axiosErr.response.data as { message: string }).message;
            }
            return axiosErr.message || 'An unexpected server error occurred';
        }
        if (err instanceof Error) {
            return err.message;
        }
        return 'An unexpected error occurred';
    }
}