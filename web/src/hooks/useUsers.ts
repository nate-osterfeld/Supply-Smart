import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { UserService } from '../api/UserService';
import type { InviteUserDto } from '../models';

export function useUsers() {
    const queryClient = useQueryClient();
    const service = new UserService();

    const usersQuery = useQuery({
        queryKey: ['tenant-users'],
        queryFn: () => service.getUsers(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const inviteMutation = useMutation({
        mutationFn: (dto: InviteUserDto) => service.inviteUser(dto),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['tenant-users'] });
            queryClient.invalidateQueries({ queryKey: ['compliance-audit-logs'] });
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (userId: number) => service.deleteUser(userId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['tenant-users'] });
            queryClient.invalidateQueries({ queryKey: ['compliance-audit-logs'] });
        },
    });

    return {
        users: usersQuery.data ?? [],
        isLoadingUsers: usersQuery.isLoading,
        inviteUser: inviteMutation.mutateAsync,
        deleteUser: deleteMutation.mutateAsync,
        isInviting: inviteMutation.isPending,
        isDeleting: deleteMutation.isPending,
    };
}