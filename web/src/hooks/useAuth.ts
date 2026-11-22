import { useMutation, useQueryClient } from '@tanstack/react-query';
import { AuthService } from '../api/AuthService';
import { sessionChanged } from '../api/browserSession';
import type { LoginDto, RegisterDto } from '../models';

export function useAuth() {
    const service = new AuthService();
    const queryClient = useQueryClient();

    const completeAuthentication = async () => {
        // Queries use shared keys, so no previous account's data may survive a new session.
        await queryClient.cancelQueries();
        queryClient.clear();
        sessionChanged(window.location.search);
    };

    const loginMutation = useMutation({
        mutationFn: (dto: LoginDto) => service.login(dto),
        onSuccess: completeAuthentication,
    });

    const registerMutation = useMutation({
        mutationFn: (dto: RegisterDto) => service.register(dto),
        onSuccess: completeAuthentication,
    });

    return {
        login: loginMutation,
        register: registerMutation,
    };
}
