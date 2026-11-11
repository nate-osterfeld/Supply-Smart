import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { NotificationService } from '../api/NotificationService';
import type { UpdateNotificationTemplateRequestDto } from '../models';


export function useNotifications() {
    const queryClient = useQueryClient();
    const service = new NotificationService();

    const templatesQuery = useQuery({
        queryKey: ['notification-templates'],
        queryFn: () => service.getTemplates(),
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const updateMutation = useMutation({
        mutationFn: (dto: UpdateNotificationTemplateRequestDto) => service.updateTemplate(dto),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['notification-templates'] });
        },
    });

    const testAlertMutation = useMutation({
        mutationFn: (dto: UpdateNotificationTemplateRequestDto) => service.sendTestAlert(dto),
    });

    return {
        templates: templatesQuery.data ?? [],
        isLoadingTemplates: templatesQuery.isLoading,
        saveTemplate: updateMutation.mutateAsync,
        isSavingTemplate: updateMutation.isPending,
        sendTestAlert: testAlertMutation.mutateAsync,
        isSendingTest: testAlertMutation.isPending,
    };
}