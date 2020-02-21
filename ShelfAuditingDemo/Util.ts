const ShelfGapName = "gap";
const UnknownProductName = "product";

const TaggedItemColor =      'rgba(36, 143, 255, 1)'; // '#248FFF';
const ShelfGapColor =        'rgba(250, 190, 20, 1)'; // '#FABE14';
const UnknownProductColor =  'rgba(180, 0, 158, 1)'; // '#B4009E';

class Util {

    static GetObjectRegionColor (model) {
        let tagName = model && model.tagName != null ? model.tagName.toLowerCase() : '';

        if (tagName == UnknownProductName.toLowerCase()) {
            return UnknownProductColor;

        } else if (tagName == ShelfGapName.toLowerCase()) {
            return ShelfGapColor;
        }

        return TaggedItemColor;
    }

    static SetOpacityToColor(rgba, opacity) {
        let newColor = rgba.replace("1)", `${opacity})`);
        return newColor;
    }

}
  
  export { Util };