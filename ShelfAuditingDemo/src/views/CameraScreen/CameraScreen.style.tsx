import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    mainContainer: {
        flex: 1, 
        flexDirection: 'column', 
        backgroundColor: 'transparent'
    },
    cameraPreview: {
        flex: 1, 
        justifyContent: 'flex-end', 
        alignItems: 'center'
    },
    cameraControlsContainer: {
        position: 'absolute',
        bottom: 0,
        alignSelf: 'center',
        opacity: 0.6
    },
    takePictureButton: {
        flex: 0,
        backgroundColor: '#fff',
        borderRadius: 25,
        alignSelf: 'center',
        margin: 20,
        width: 50,
        height: 50
    },
    horizontalLineContainer: {
        position: 'absolute', 
        width: '100%', 
        height: '100%'
    },
    verticalLineContainer: {
        position: 'absolute', 
        flexDirection: 'row', 
        width: '100%', 
        height: '100%'
    },
    horizontalLineView: {
        flex: 1, 
        justifyContent: 'flex-end'
    },
    verticalLineView: {
        flex: 1, 
        flexDirection: 'row', 
        justifyContent: 'flex-end'
    },
    horizontalLine: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.6
    },
    verticalLine: {
        width: 1, 
        backgroundColor: 'white', 
        opacity: 0.6
    }
})
