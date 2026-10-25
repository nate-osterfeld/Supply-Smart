import { BaseApi } from './BaseApi';
import type { UserDto, InviteUserDto } from '../models';

export interface IUserService {
    getUsers(): Promise<UserDto[]>;
    inviteUser(dto: InviteUserDto): Promise<UserDto>;
    deleteUser(userId: number): Promise<void>;
}

export class UserService extends BaseApi implements IUserService {
    public constructor() {
        super(true);
    }

    public getUsers = async (): Promise<UserDto[]> => {
        const response = await this.get<UserDto[]>('api/settings/users');
        return response.data;
    };

    public inviteUser = async (dto: InviteUserDto): Promise<UserDto> => {
        const response = await this.post<UserDto>('api/settings/users/invite', dto);
        return response.data;
    };

    public deleteUser = async (userId: number): Promise<void> => {
        await this.delete<void>(`api/settings/users/${userId}`);
    };
}