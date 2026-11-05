import { BaseApi } from './BaseApi'

export interface EmailTemplateDraft {
  subject: string
  body: string
  revision: number
}
export interface EmailTemplate extends EmailTemplateDraft {
  trigger: string
  label: string
  supportedVariables: string[]
  updatedAtUtc: string | null
  updatedBy: string | null
}
export class EmailTemplateService extends BaseApi {
  async getTemplates(): Promise<EmailTemplate[]> {
    return (await this.get<EmailTemplate[]>('api/settings/email-templates')).data
  }
  async saveTemplate(trigger: string, draft: EmailTemplateDraft): Promise<EmailTemplate> {
    return (await this.put<EmailTemplate>(`api/settings/email-templates/${encodeURIComponent(trigger)}`, draft)).data
  }
  async sendTest(trigger: string, draft: EmailTemplateDraft): Promise<{ recipient: string }> {
    return (await this.post<{ recipient: string }>(`api/settings/email-templates/${encodeURIComponent(trigger)}/test`, draft)).data
  }
}
