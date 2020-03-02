import React from 'react'
import { View, Platform, TouchableOpacity } from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { RNCamera } from 'react-native-camera'
import { ZoomView } from '../../components'
import { styles } from './CameraScreen.style'
import { ImagePickerType } from '../../models/ImagePickerType'

const MAX_ZOOM = 7; // iOS only
const ZOOM_F = Platform.OS === 'ios' ? 0.007 : 0.08;

interface CameraProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface CameraState {
    zoom: number;
    isCameraVisible: boolean;
    specData: any;
}

export class CameraScreen extends React.Component<CameraProps, CameraState> {
    private camera: any;
    private _prevPinch: number;

    constructor(props: CameraProps) {
        super(props);
        this.state = {
            zoom: 0.0,
            isCameraVisible: false,
            specData: {}
        }

        this._prevPinch = 1;
    }

    componentDidMount() {
        const { navigation } = this.props;
        let specData = navigation.getParam('specData', 'unknown');

        this.setState({
            zoom: 0.0,
            specData: specData
        });
    }

    render() {
        let horizontalLines = 
            <View style={styles.horizontalLineContainer}>
                <View style={styles.horizontalLineView}>
                    <View style={styles.horizontalLine} />
                </View>
                <View style={styles.horizontalLineView}>
                    <View style={styles.horizontalLine} />
                </View>
                <View style={styles.horizontalLineView} />
            </View>;

        let verticalLines = 
            <View style={styles.verticalLineContainer}>
                <View style={styles.verticalLineView}>
                    <View style={styles.verticalLine} />
                </View>
                <View style={styles.verticalLineView}>
                    <View style={styles.verticalLine} />
                </View>
                <View style={styles.verticalLineView} />
            </View>;

        let cameraControls =
            <View style={styles.cameraControlsContainer}>
                <TouchableOpacity onPress={this.takePicture.bind(this)} style={styles.takePictureButton} />                       
            </View>

        return (
            <View style={styles.mainContainer}>
                <RNCamera 
                    ref={ref => { this.camera = ref; }} 
                    style={styles.cameraPreview}
                    zoom={this.state.zoom}
                    maxZoom={MAX_ZOOM}
                    flashMode={RNCamera.Constants.FlashMode.auto}
                    androidCameraPermissionOptions={{
                        title: 'Permission to use camera',
                        message: 'We need your permission to use your camera',
                        buttonPositive: 'Ok',
                        buttonNegative: 'Cancel',
                    }}
                    androidRecordAudioPermissionOptions={{
                        title: 'Permission to use audio recording',
                        message: 'We need your permission to use your audio',
                        buttonPositive: 'Ok',
                        buttonNegative: 'Cancel',
                    }}>

                    <ZoomView
                        onPinchEnd={this.onPinchEnd}
                        onPinchStart={this.onPinchStart}
                        onPinchProgress={this.onPinchProgress}>

                        {horizontalLines}
                        {verticalLines}

                        {cameraControls}
                    </ZoomView>

                </RNCamera>
            </View>
        );
    }

    async takePicture() {
        if (this.camera) {
            const { navigate } = this.props.navigation;
            
            const options = { quality: 0.5, base64: true };
            const data = await this.camera.takePictureAsync(options);

            this.setState({ isCameraVisible: false });
            navigate('Review', {image: data, selectedSpec: this.state.specData, imagePickerType: ImagePickerType.FromCamera });
        }
    }

    onPinchStart = () => {
        this._prevPinch = 1;
    }
    
    onPinchEnd = () => {
        this._prevPinch = 1;
    }
    
    onPinchProgress = (p: number) => {
        let p2 = p - this._prevPinch;
        if (p2 > 0 && p2 > ZOOM_F) {
          this._prevPinch = p;
          this.setState({zoom: Math.min(this.state.zoom + ZOOM_F, 1)}, () => { });
        } else if (p2 < 0 && p2 < -ZOOM_F) {
          this._prevPinch = p;
          this.setState({zoom: Math.max(this.state.zoom - ZOOM_F, 0)}, () => { });
        }
    }
}
