import type { JournalItem } from '@/models/JournalItem';

const mockEntries: JournalItem[] = [
    {
        id: 1,
        text: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus nulla magna, luctus vitae ligula vitae, ullamcorper dictum nisl. Integer tempus a lacus at finibus. Pellentesque nulla ipsum, tristique quis turpis et, semper euismod sem.',
        date: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        text: 'Proin erat metus, consectetur et ornare at, mollis non quam. Nam vel magna dolor. Nulla molestie lacus vitae arcu maximus cursus. Etiam in suscipit lorem, dapibus varius tellus. Vestibulum pulvinar mollis ligula, nec rhoncus nulla lobortis sed. Morbi feugiat justo sit amet placerat vestibulum. Sed et tortor non erat molestie aliquam. Nullam tincidunt ultrices pretium. ',
        goalId: 1,
        date: new Date("December 26, 2025 13:30:00")
    },
    {
        id: 3,
        text: `Curabitur nec sapien sed augue maximus laoreet. Praesent orci arcu, scelerisque at enim in, placerat scelerisque justo. Donec ullamcorper lorem nec dui ultricies laoreet. Integer libero ex, viverra ut dui ut, sagittis convallis felis. Morbi iaculis interdum sagittis. Nulla finibus congue magna ac mattis. Morbi eget odio eu tellus vulputate elementum. Vivamus eu interdum neque.

Maecenas eget urna sem. Nunc porta justo ac semper vulputate. Cras nunc lorem, malesuada in sem eu, tempor tempus sapien. Donec volutpat iaculis risus ut scelerisque. In cursus turpis nec consectetur mollis. Quisque consectetur, velit quis consectetur ultrices, dolor odio condimentum nunc, sed posuere lacus augue eget eros. Nam in mollis est, et consequat ex. `,
        addictionId: 1,
        date: new Date("December 29, 2025")
    },
    {
        id: 4,
        text: 'Suspendisse pretium sem non pretium scelerisque. Fusce non enim velit. Pellentesque sed hendrerit nisl. Donec at ornare arcu. Cras congue facilisis diam quis sagittis. Nullam lacinia justo molestie lorem facilisis, id ultrices tellus finibus. Quisque vel lacus nunc. Integer posuere pellentesque nulla eu feugiat. ',
        goalId: 1,
        addictionId: 1,
        date: new Date("December 20, 2025")
    },
];

export const journalApi = {
    async getItems(): Promise<JournalItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockEntries), 300)
        })
    },
}