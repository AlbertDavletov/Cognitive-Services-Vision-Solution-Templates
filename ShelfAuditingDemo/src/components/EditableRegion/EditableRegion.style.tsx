import { StyleSheet } from 'react-native'

const CircleSize = 24;

export const styles = StyleSheet.create({
    circleStyle: {
        position: 'absolute',
        width: CircleSize,
        height: CircleSize,
        backgroundColor: 'green',
        borderRadius: CircleSize / 2,
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
