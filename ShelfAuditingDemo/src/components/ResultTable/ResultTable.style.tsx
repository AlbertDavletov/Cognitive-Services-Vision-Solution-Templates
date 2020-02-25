import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    container: { flex: 1 },
    tableHeader: {
        flexDirection: 'row', 
        marginBottom: 10
    },
    text: {
        color: 'white', 
        opacity: 0.6
    },
    headerLine: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.6
    },
    rowLine: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.2
    },
    imageThumbnail: {
        justifyContent: 'center',
        alignItems: 'center',
        height: 40,
        width: 40
    }
})
