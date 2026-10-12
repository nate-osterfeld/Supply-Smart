export interface ContainerDto {
    id: number;
    name: string;
    kind: string;
    parentContainerId?: number | null;
}