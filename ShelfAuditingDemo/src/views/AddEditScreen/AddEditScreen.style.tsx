import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    mainContainer: {
        flex: 1,
        backgroundColor: 'black',
        padding: 0
    },
    labelContainer: {
        height: '25%',
        padding: 16
    },
    imageThumbnail: {
        justifyContent: 'center',
        alignItems: 'center',
        height: 40,
        width: 40
    },
    button: {
        padding: 6, 
        marginBottom: 10
    },
    buttonLabel: {
        color: '#0078D4', 
        fontSize: 16, 
        fontWeight: 'bold', 
        textAlign: 'center' 
    },
    tagContainer: {
        flex: 1, 
        justifyContent: 'center'
    },
    tagLabel: {
        color: 'white', 
        textAlign: 'left', 
        padding: 5
    },
    line: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.2
    }
})
