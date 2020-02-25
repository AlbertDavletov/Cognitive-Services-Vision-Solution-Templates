import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    activeRegion: {
        justifyContent: 'center',
        borderWidth: 1,
        borderRadius: 1
    },
    selectedRegion: {
        borderWidth: 2,
        backgroundColor: 'transparent',
        borderRadius: 2
    },
    disabledRegion: {
        justifyContent: 'center',
        borderWidth: 0,
        borderRadius: 1
    },
    selectedRegionLabelPanel: {
        top: -32, 
        borderWidth: 0,
        margin: -2,
        borderRadius: 1,
        minWidth: 100,
    },
    selectedRegionLabel: {
        color: 'white',
        padding: 4
    },
    circle: {
        width: 12,
        height: 12,
        borderRadius: 6,
        backgroundColor: 'rgba(255, 255, 255, 0.8)',
        alignSelf: 'center'
    },
    smallCircle: {
        width: 8,
        height: 8,
        borderRadius: 4,
        alignSelf: 'center'
    }
})
