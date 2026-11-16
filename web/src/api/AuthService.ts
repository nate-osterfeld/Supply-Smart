import { BaseApi } from './BaseApi';
import type { RegisterDto, LoginDto, AuthResponseDto, LocationDto } from '../models';

export interface IAuthService {
    login(dto: LoginDto): Promise<AuthResponseDto>;
    register(dto: RegisterDto): Promise<AuthResponseDto>;
    logout(): Promise<void>;
    getLocations(): Promise<LocationDto[]>;
}

export class AuthService extends BaseApi implements IAuthService {
    public constructor() { 
        super(true); // usesAuth = true for protected routes like locations
    }

    private readonly endpoint: string = 'api/auth';

    public login = async (dto: LoginDto): Promise<AuthResponseDto> => {
        // Login doesn't need prior auth token, so we override usesAuth or use a separate instance if preferred
        const response = await this.post<AuthResponseDto>(`${this.endpoint}/login`, dto);
        return response.data;
    };

    public register = async (dto: RegisterDto): Promise<AuthResponseDto> => {
        const response = await this.post<AuthResponseDto>(`${this.endpoint}/register`, dto);
        return response.data;
    };

    public logout = async (): Promise<void> => {
        await this.post<void>(`${this.endpoint}/logout`);
    };

    public getLocations = async (): Promise<LocationDto[]> => {
        const response = await this.get<LocationDto[]>(`${this.endpoint}/locations`);
        return response.data;
    };
}
