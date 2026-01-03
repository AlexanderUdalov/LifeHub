import type { AddictionItem } from "@/models/AddictionItem";
import type { ColoredTagEntity } from "@/models/ColoredTagEntity";

const mockAddictions: AddictionItem[] = [
    {
        id: 1,
        title: 'Smoking',
        icon: 'pi-ban',
        lastReset: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        title: 'Drinking',
        icon: 'pi-ban',
        lastReset: new Date("December 20, 2025 13:30:00")
    },
    {
        id: 3,
        title: 'Serfing',
        icon: 'pi-ban',
        lastReset: new Date()
    }
];

const mockAddictionsAsTags: ColoredTagEntity[] = [
    {
        id: 1,
        title: 'Smoking',
        // icon: 'pi-ban',
        color: "#17BEBB"
    },
    {
        id: 2,
        title: 'Drinking',
        // icon: 'pi-ban',
        color: "#D62246"
    },
    {
        id: 3,
        title: 'Serfing',
        // icon: 'pi-ban',
        color: "#4B1D3F"
    }
];

export const addictionsApi = {
    async getAddictions(): Promise<AddictionItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockAddictions), 300)
        })
    },

    async getAddictionsAsTags(): Promise<ColoredTagEntity[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockAddictionsAsTags), 300)
        })
    }
}