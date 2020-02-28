import { StyleSheet } from 'react-native'

export const TouchableCircleSize = 42;
export const VisibleCircleSize = 24;
export const CircleButtonActiveColor = 'white';
export const CircleButtonPressedColor = 'gray';

export const styles = StyleSheet.create({
    touchableCircleStyle: {
        position: 'absolute',
        backgroundColor: 'transparent',
        width: TouchableCircleSize,
        height: TouchableCircleSize,
        borderRadius: TouchableCircleSize / 2,
        alignItems: 'center', 
        justifyContent: 'center'
    },
    visibleCircleStyle: {
        width: VisibleCircleSize,
        height: VisibleCircleSize,
        borderRadius: VisibleCircleSize / 2, 
    },
    selectedRegion: {
        flex: 1,
        borderWidth: 2,
        backgroundColor: 'transparent',
        borderRadius: 2,
        borderColor: 'white',
        borderStyle: 'dashed'
    },
    selectedRegionLabelPanel: {
        top: -42, 
        borderWidth: 0,
        margin: -2,
        borderRadius: 1,
        minWidth: 100,
        backgroundColor: 'white'
    },
    selectedRegionLabel: {
        color: 'black',
        padding: 4
    }
})
