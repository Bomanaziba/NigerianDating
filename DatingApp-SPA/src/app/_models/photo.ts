export interface Photo {
    id: number;
    description: string;
    dateAdded: Date;
    isMain: boolean;
    url: string;
    isApproved: boolean;
}
