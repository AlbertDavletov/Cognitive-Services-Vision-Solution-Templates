import { UnknownProduct, ShelfGap } from './constants'

const TaggedItemColor =      'rgba(36, 143, 255, 1)'; // '#248FFF';
const ShelfGapColor =        'rgba(250, 190, 20, 1)'; // '#FABE14';
const UnknownProductColor =  'rgba(180, 0, 158, 1)';  // '#B4009E';

export class Util {

    static GetObjectRegionColor (model: any) {
        let tagName = model && model.tagName != null ? model.tagName.toLowerCase() : '';

        if (tagName === UnknownProduct.toLowerCase()) {
            return UnknownProductColor;

        } else if (tagName === ShelfGap.toLowerCase()) {
            return ShelfGapColor;
        }

        return TaggedItemColor;
    }

    static SetOpacityToColor(rgba: string, opacity: number) {
        let newColor = rgba.replace("1)", `${opacity})`);
        return newColor;
    }

}
