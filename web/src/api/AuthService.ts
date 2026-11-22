import { BaseApi } from './BaseApi';
import { csrfHeader } from './browserSession';
import type { RegisterDto, LoginDto, AuthResponseDto } from '../models';

export interface IAuthService {
    login(dto: LoginDto): Promise<AuthResponseDto>;
    register(dto: RegisterDto): Promise<AuthResponseDto>;
    logout(): Promise<void>;
}

export class AuthService extends BaseApi implements IAuthService {
    private readonly endpoint: string = 'api/auth';

    public login = async (dto: LoginDto): Promise<AuthResponseDto> => {
        const response = await this.post<AuthResponseDto>(`${this.endpoint}/session/login`, dto, {
            headers: { [csrfHeader]: 'start-session' },
            // A credential rejection belongs on the sign-in form, not in the expired-session interceptor.
            validateStatus: status => status === 200 || status === 401,
        });
        if (response.status === 401) throw new Error('Email or password is incorrect.');
        return response.data;
    };

    public register = async (dto: RegisterDto): Promise<AuthResponseDto> => {
        const response = await this.post<AuthResponseDto>(`${this.endpoint}/session/register`, dto, {
            headers: { [csrfHeader]: 'start-session' },
        });
        return response.data;
    };

    public logout = async (): Promise<void> => {
        await this.post<void>(`${this.endpoint}/logout`);
    };

}
