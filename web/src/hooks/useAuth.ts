import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AuthService } from '../api/AuthService';
import type { LoginDto, RegisterDto, AuthResponseDto, LocationDto } from '../models';

export function useAuth() {
    const service = new AuthService();
    const queryClient = useQueryClient();

    const completeAuthentication = async (data: AuthResponseDto) => {
        // Queries use shared keys, so no previous account's data may survive a new session.
        await queryClient.cancelQueries();
        queryClient.clear();
        localStorage.removeItem('supplysmart_location_id');
        localStorage.setItem('supplysmart_token', data.token);
        localStorage.setItem('supplysmart_org_id', data.organizationId.toString());
        localStorage.setItem('supplysmart_role', data.role);
        localStorage.setItem('supplysmart_email', data.email);
        localStorage.setItem('supplysmart_name', data.fullName);
    };

    const loginMutation = useMutation({
        mutationFn: (dto: LoginDto) => service.login(dto),
        onSuccess: completeAuthentication,
    });

    const registerMutation = useMutation({
        mutationFn: (dto: RegisterDto) => service.register(dto),
        onSuccess: completeAuthentication,
    });

    const locationsQuery = useQuery({
        queryKey: ['locations'],
        queryFn: () => service.getLocations(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    return {
        login: loginMutation,
        register: registerMutation,
        locations: (locationsQuery.data ?? []) as LocationDto[],
        isLoadingLocations: locationsQuery.isLoading,
    };
}
