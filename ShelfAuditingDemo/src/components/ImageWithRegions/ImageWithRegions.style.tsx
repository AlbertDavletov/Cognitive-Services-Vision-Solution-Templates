import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    imageContainer: {
        flex: 1
    },
    image: {
        flex: 1,
        justifyContent: 'center',
        transform: [{ scale: 1 }]
    },
    touchableContainer: {
        position: 'absolute',
        justifyContent: 'center'
    },
    canvasContainer: {
        alignSelf: 'center'
    },
    editCanvasContainer: {
        width: '100%', 
        height: '100%', 
        backgroundColor: 'rgba(0, 0, 0, 0.0)', 
        overflow: 'hidden', 
        zIndex: 20
    }
})
