export interface CreateContainerDto {
  name: string;
  kind: string;
  parentContainerId?: number | null;
}